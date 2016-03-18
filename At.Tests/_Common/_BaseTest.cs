﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq.Expressions;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Reflection;

namespace At.Tests
{

//Test base class
[TestClass] public partial class Test
{
   //ctor
   protected Test()
   {
      At.Tests.TestData.Init();
   }

   //Stopwatch
   public Stopwatch Stopwatch {get {return stopwatch;}}
   Stopwatch stopwatch = new Stopwatch();
   
   //TestContext
   public TestContext TestContext {get; set;}

   //TestContextWriter
   public TestContextTextWriter TestContextWriter
   { get
      { if (_TestContextWriter == null) _TestContextWriter = new TestContextTextWriter(this);
         return _TestContextWriter;
      }
   } TestContextTextWriter _TestContextWriter;


   //Initialize()
   [TestInitialize] public void Initialize()
   { Setup();
   }
   
   //Cleanup()
   [TestCleanup] public void Cleanup() 
   {
      if (_TestContextWriter != null && _TestContextWriter.Buffered) _TestContextWriter.flush2();
      TearDown();
   }
  
   //TearDown()
   protected virtual void TearDown()
   { 
   }

    //assert_equals()
    public void assert_equals<T>(T expected, T actual, string f = null, params object[] a)
    {
        assert_equals(()=>expected,()=>actual,f,a);
    }
    public void assert_equals<T>(T expected, Expression<Func<T>> actual, string f = null, params object[] a)
    {
        assert_equals(()=>expected,actual,f,a);
    }
    public void assert_equals<T>( Expression<Func<T>> expected
                                 ,Expression<Func<T>> actual
                                 ,string              format = null
                                 ,params object[]     args) 
   { Write("assert EQUAL: {0} == {1}",exprStr(expected.Body),exprStr(actual.Body));
     var f = expected.Compile();
     var g = actual.Compile();
     var x = f();
     var y = g();

     if (format == null)
        Assert.AreEqual(x,y);
     else
        Assert.AreEqual(x,y,string.Format(format,args));
   } 

  //assert_true()
  public void assert_true(Expression<Func<bool>> e) 
   { var b = e.Body;
     Write("assert TRUE: {0}",exprStr(e.Body));

     switch(b.NodeType)
      { 
        case ExpressionType.Equal:
         {
           var be = (BinaryExpression) b;
           var l1 = Expression.Lambda(be.Left);
           var l2 = Expression.Lambda(be.Right);
           var f1 = l1.Compile();
           var f2 = l2.Compile();
           var v1 = f1.DynamicInvoke();
           var v2 = f2.DynamicInvoke();
           Assert.AreEqual(v2,v1);
           break;
         }

        default: var f = e.Compile();
                 var x = f();
                 Assert.IsTrue(x);
                 break;
      }


   } 
   
  //assert_type()
  public void assert_type<T>(Expression<Func<object>> e)
  {
     var t = typeof(T);
     Write("assert TYPE({0}): {1}",t,exprStr(e.Body));
     var f = e.Compile();
     var x = f();
     Assert.IsInstanceOfType(x, t);
  }

  //assert_false()
  public void assert_false(Expression<Func<bool>> e) 
   { Write("assert FALSE: {0}",exprStr(e.Body));
     var f = e.Compile();
     var x = f();
     Assert.IsFalse(x);
   } 

   
  //ASSERT_NULL()
  public void assert_null<T>(Expression<Func<T>> e) where T : class
   { Write("assert NULL: {0}",exprStr(e.Body));
     var f = e.Compile();
     var x = f();
     Assert.IsNull(x);
   }
   
  //AssertNotNull
  public void assert_not_null<T>(Expression<Func<T>> e) where T : class
   { Write("assert NOT NULL: {0}",exprStr(e.Body));
     var f = e.Compile();
     var x = f();
     Assert.IsNotNull(x);
   }

   //Property(username, defaultValue)
   protected string Property(string name, string defaultValue)
   { var setting =  TestContext.Properties.Contains(name) 
                     ? TestContext.Properties[name].ToString()
                     : null;
      return (!string.IsNullOrEmpty(setting)) ? setting : defaultValue;
   }

   //Property(username)
   /// <summary>For test properties that may have null or empty string as a legal value,
   /// use the overload that accepts a default value.</summary>
   protected  string Property(string name)
   { var setting =  TestContext.Properties.Contains(name) 
                     ? TestContext.Properties[name].ToString()
                     : null;

      if (!string.IsNullOrEmpty(setting))
      { 
         throw new Exception("Test property "+setting+" is null.");     
      }
     
      return setting;
   }

   
   //Setup()
   protected virtual void Setup()
   { 

   }

   //Wait()
   protected internal void Wait(float seconds)
   {
      Thread.Sleep((int)(seconds*1000f));
   }
   
   //Write()
   protected internal void Write(string f, params object[] args)
   { 
      TestContext.WriteLine("[{0}] {1}",DateTime.Now, string.Format(f,args));
   }
   
   //Write()
   protected internal void Write(object o)
   { TestContext.WriteLine("[{0}] {1}",DateTime.Now,o);
   }


 
    //exprString() 
    string exprStr(Expression e)
    {
        if (e==null) return "<null expression>";

        switch(e.NodeType)
        {

            //AndAlso (&&)
            case ExpressionType.AndAlso: return binaryStr(e,"&&");


            //Call
            case ExpressionType.Call:
            {
                var mce = (MethodCallExpression) e;
                var m   = mce.Method;

                return   (m.Name=="get_Item") ? 
                            exprStr(mce.Object)+"."+"["+exprStr(mce.Arguments[0])+"]" : 
                         (m.DeclaringType==typeof(Enumerable)) ?
                            $"{exprStr(mce.Arguments[0])}.{mce.Method.Name}({string.Join(",",mce.Arguments.Skip(1).Select(exprStr))})" :
                         (m.IsStatic) 
                            ? m.DeclaringType.Name 
                            : exprStr(mce.Object)+"."+m.Name+"("+string.Join(",",mce.Arguments.Select(exprStr))+")";
        
            }

            //Constant
            case ExpressionType.Constant: 
            {
                var ce = (ConstantExpression) e;
                var t  = ce.Value.GetType();
                var v  = ce.Value;

                return    (t==typeof(int))                  ? v.ToString()
                        : (t==typeof(string))               ? "\"" + v.ToString().Replace("\"","\\\"") + "\""
                        : (t.Name.Contains("DisplayClass")) ? ""
                        : $"[{ce.Type} {ce.Value.ToString()}]";
            }

            //Convert
            case ExpressionType.Convert:
            {
                var ue = (UnaryExpression) e;
                return "(("+ue.Type.Name+") "+exprStr(ue.Operand)+")";
            }

            //Equal (==)
            case ExpressionType.Equal: return binaryStr(e,"==");
            

            //Lambda
            case ExpressionType.Lambda:
            {
                var le = (LambdaExpression) e;
                var ps = le.Parameters.Count==1 ? 
                            le.Parameters[0].Name : 
                            $"({string.Join(", ",le.Parameters.Select(_=>_.Name))})";
                return $"{ps} => {exprStr(le.Body)}";
            }

            //Member
            case ExpressionType.MemberAccess:
            {
                var mae = (MemberExpression) e;
                var m   = mae.Member;
                var o   = exprStr(mae.Expression);

                if (m.Name=="expected" || m.Name=="actual")
                    return getValue(mae).ToString();
                else
                    return  o.Length == 0 ? m.Name : $"{o}.{m.Name}";
            }

            //Parameter
            case ExpressionType.Parameter: return ((ParameterExpression)e).Name;

            //DEFAULT
            default: Write("exprString(): "+e.NodeType) ; return e.ToString();
        }
    }

    object getValue(MemberExpression me)
    {
        var m = me.Member;
        var p = m as PropertyInfo;
        var o = Expression.Lambda(me.Expression).Compile().DynamicInvoke();

        return
            (p != null) ? 
                p.GetValue(o) :

            ((FieldInfo)m).GetValue(o);
    }

    string binaryStr(Expression e, string op)
    {
        var be = (BinaryExpression) e;
        return $"{exprStr(be.Left)} {op} {exprStr(be.Right)}";
    }

    //testEmailAddress()
    public string testEmailAddress 
    { get 
        { if (m_testEmailAddress == null)  m_testEmailAddress = "test@example.com";
            return m_testEmailAddress; 
        }
    } string m_testEmailAddress;

}

//TestContextTextWriter class
public class TestContextTextWriter : TextWriter
{
   readonly Test test;

   List<char> charBuffer   = new List<char>();
   List<string> lineBuffer = new List<string>(); //if Buffered = true

   //.ctor (TestContextTextWriter)
   internal TestContextTextWriter(Test test) 
   { this.test = test;
      Buffered = false;
      BufferedMaxOutput = 5;
   }

   //BufferedMaxOutput
   /// <summary>Maximum number of lines to write if Buffered = true</summary>
   public int BufferedMaxOutput {get; set;}

   //Buffered
   /// <summary>If true, output is buffered until end of test. Calling Flush()
   /// won't flush the buffer.</summary>
   public bool Buffered {get; set;}

   public override void Write(char value) 
   { if (Buffered) lineBuffer[lineBuffer.Count-1] += value;      
      else charBuffer.Add(value); 
   }

   public override void Flush() 
   { if (charBuffer.Count==0) return;
      WriteLine(new string(charBuffer.ToArray()));
      charBuffer.Clear();
   }

   public override void WriteLine()
   { Flush();
      if (Buffered) Write(new string(base.CoreNewLine));
      else test.Write("");       
   }

   public override void WriteLine(string value)
   { Flush();
      if (Buffered) lineBuffer.Add(value);
      else test.Write(value);
   }
   
   public override Encoding  Encoding {get {  return Encoding.Default; }}

   //called from test.Cleanup()
   internal void flush2()
   { foreach(var s in lineBuffer.Skip(lineBuffer.Count-BufferedMaxOutput)) test.Write(s);
   }
}
}
